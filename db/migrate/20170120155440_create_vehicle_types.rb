class CreateVehicleTypes < ActiveRecord::Migration[5.0]
  def change
    create_table :vehicle_types do |t|
      t.string :name, null: false
	  t.integer :mark_count, null: false, default: 0

      t.timestamps
    end

	change_column :vehicle_types, :id, :string
	
  end
end
